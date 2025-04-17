using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Animations : Chessboard
{
	public const float animationSpeed = 0.3f;
	public static bool promotionUnsafe = false, CancelCheckEarly = false, CancelCastlingEarly = false, ActiveCheckAnimation = false;
	public static List<Vector2I> PreviousCheckTiles = new(), CheckAnimationsStarted = new();
	public static Dictionary<Tween, (Sprite2D spr, bool deleteOnFinish, float? transparency)> ActiveTweens = new();
	public static int firstCheckZone = 0;
	public const float lowAnimationDurationBoundary = 0.3f;
	public static void CursorTween(Sprite2D spr, float duration, Vector2I startPosition, Vector2? endPosition, float? endTransparency) => Tween(spr, duration, startPosition, endPosition, null, endTransparency, false, false, true, -1, -1, false, Godot.Tween.TransitionType.Linear, null, Layer.Cursor);
	private static void TagComponentTween(Sprite2D component, float duration, Vector2I startPosition, Vector2? endPosition, float? endTransparency, Layer layer, int tagIndex) => Tween(component, duration, startPosition, endPosition, null, endTransparency, false, layer: layer, tagIndex: tagIndex);
	public static void TagTween(Vector2I startPosition, Vector2? endPosition, float duration, int chainIterator = -1, int castlingAnimation = -1, Promotion.TagAnimation promotionForTags = Promotion.TagAnimation.None, bool deleting = true)
    {
        if (promotionForTags == Promotion.TagAnimation.Replay || !Tags.visibleTags.ContainsKey(startPosition)) return;
		int tagIndex = -1;
		List<Tags.VisibleTag> visibleTagsAtPosition = new();
		bool promotionShowingChoices = promotionForTags == Promotion.TagAnimation.ShowingChoices;
		foreach (Tags.VisibleTag visibleTag in Tags.visibleTags[startPosition])
		{
			float usedTransparency = deleting ? 1 : 0;
			if (Tags.lastHandledTags.ContainsKey(startPosition) && Tags.lastHandledTags[startPosition].Contains(visibleTag.Tag) || promotionShowingChoices)
				usedTransparency = deleting || promotionShowingChoices ? 0 : 1;
			else visibleTagsAtPosition.Add(visibleTag);
			TagComponentTween(visibleTag.TagVisualizer, duration, startPosition, endPosition, usedTransparency, Layer.TagVisualizer, tagIndex);
			TagComponentTween(visibleTag.TagEmblem, duration, startPosition, endPosition, usedTransparency, Layer.TagEmblem, tagIndex);
			tagIndex = tagIndex == -1 ? 1 : 0;
		}
		if (chainIterator > 1 || endPosition == null || promotionForTags != Promotion.TagAnimation.None) return;
        Tags.visibleTags.Remove(startPosition);
		if (visibleTagsAtPosition.Count > 0)
            Tags.visibleTags.Add(castlingAnimation == -1 ? (Vector2I)endPosition : new(Castling.endXPositions[castlingAnimation], startPosition.Y), visibleTagsAtPosition);
    }
	public static void Tween(Sprite2D spr, float duration, Vector2I startPosition, Vector2? endPosition, float? endScale, float? endTransparency, bool deleteOnFinished, bool promotion = false, bool deleteFromPiecesDict = true, int chainIterator = -1, int castlingAnimation = -1, bool promotionConfirmation = false, Tween.TransitionType transition = Godot.Tween.TransitionType.Sine, Tween.EaseType? easeType = Godot.Tween.EaseType.InOut, Layer layer = Layer.Piece, int tagIndex = 0, Promotion.TagAnimation promotionForTags = Promotion.TagAnimation.None)
	{
		Tween tween = spr.CreateTween();
		ActiveTweens.Add(tween, (spr, deleteOnFinished, endTransparency));
		if (endPosition != null && layer != Layer.Cursor)
			spr.ZIndex = (int)Layer.Promotion;
		if (layer == Layer.Piece)
			TagTween(startPosition, endPosition, duration, chainIterator, castlingAnimation, promotionForTags);
		if (endPosition != null)
		{
			Vector2 usedEndPos = (Vector2)endPosition;
			TweenSetup(spr, tween, "position", CalculateTilePosition(usedEndPos, layer, tagIndex), duration, transition, easeType);
		}
		if (endScale != null)
		{
			float usedEndScale = (float)endScale;
			float rescaledForScreenSize = usedEndScale * gridScale / svgScale;
			TweenSetup(spr, tween, "scale", new Vector2(rescaledForScreenSize, rescaledForScreenSize), duration, transition, easeType);
		}
		if (endTransparency != null)
		{
			float usedEndTransparency = (float)endTransparency;
			TweenSetup(spr, tween, "modulate:a", usedEndTransparency, duration, transition, easeType);
		}
		OnFinishedDelete(spr, startPosition, tween, endPosition, duration, deleteOnFinished, promotion, deleteFromPiecesDict, chainIterator, castlingAnimation, promotionConfirmation, layer);
	}
	private static void TweenSetup(Sprite2D spr, Tween tween, string type, Variant end, float duration, Tween.TransitionType transition, Tween.EaseType? easeType)
	{
		PropertyTweener property = tween.Parallel().TweenProperty(spr, type, end, duration).SetTrans(transition);
		if (easeType != null) property.SetEase(easeType ?? default);
	}
	private static void OnFinishedDelete(Sprite2D spr, Vector2I startPosition, Tween tween, Vector2? endPosition, float duration, bool deleteOnFinished, bool promotion, bool deleteFromPiecesDict, int chainIterator, int castlingAnimation, bool promotionConfirmation, Layer layer)
	{
		if (deleteOnFinished && deleteFromPiecesDict)
			UpdatePosition.DeletePiece(startPosition, (Vector2I?)endPosition, false, false, '\0', spr);
		if (promotion)
			promotionUnsafe = true;
		if (duration == 0)
		{
			AnimationEnd(tween, deleteOnFinished, spr, endPosition, layer);
			Promotion.MoveHistoryDisable = false;
			return;
		}
		Timer timer = new() { WaitTime = duration, OneShot = true };
		spr.AddChild(timer);
		timer.Timeout += () =>
		{
			timer.QueueFree();
			if (chainIterator != -1 && chainIterator < Castling.elipseQuality && !CancelCastlingEarly)
			{
				castlingAnimation = castlingAnimation == Castling.endXPositions.Count ? castlingAnimation - 1 : castlingAnimation;
				Vector2 updatedElipsePointPosition = Castling.CalculatePointOnElipse(chainIterator + 1, startPosition, Castling.endXPositions[castlingAnimation], Castling.elipsePathUp[castlingAnimation]);
				TagTween(new(Castling.endXPositions[castlingAnimation], startPosition.Y), updatedElipsePointPosition, duration, chainIterator, castlingAnimation);
				Tween(spr, animationSpeed / Castling.elipseQuality, startPosition, updatedElipsePointPosition, null, null, false, false, false, chainIterator + 1, castlingAnimation);
				ActiveTweens.Remove(tween);
				return;
			}
			promotionUnsafe = false;
			if (castlingAnimation >= 0)
			{
				Castling.endXPositions.RemoveAt(0);
				Castling.elipsePathUp.RemoveAt(0);
			}
			AnimationEnd(tween, deleteOnFinished, spr, endPosition, layer);
			if (Promotion.promotionPending != null)
				Promotion.Promote((Vector2I)Promotion.promotionPending);
			if (promotionConfirmation)
				Promotion.MoveHistoryDisable = false;
		};
		timer.Start();
	}
	private static void AnimationEnd(Tween tween, bool deleteOnFinished, Sprite2D spr, Vector2? endPosition, Layer layer)
	{
		ActiveTweens.Remove(tween);
		if (deleteOnFinished)
			spr.QueueFree();
		spr.ZIndex = (int)layer;
		firstCheckZone = 0;
		if (Position.GameEndState != Position.EndState.Ongoing && Position.GameEndState != Position.EndState.Checkmate) return;
		if (ActiveTweens.Count == 0 && Promotion.PromotionOptionsPieces.Count == 0 && History.activeMoveSuccessionTimers == 0)
			FlipBoard();
		if (endPosition == null)
			return;
		Vector2I endNotNull = (Vector2I)endPosition;
		for (int i = 0; i < LegalMoves.RoyalAttackers.Count; i++)
		{
			if (LegalMoves.RoyalAttackers[i] == endNotNull)
				CheckAnimation(1, ((SceneTree)Engine.GetMainLoop()).CurrentScene, i);
		}
	}
	public static void CheckAnimation(int i, Node main, int j, float durationMultiplier = 1)
	{
		bool isCheckmate = Position.GameEndState == Position.EndState.Checkmate && History.RedoMoves.Count == 0;
		if (animationSpeed == 0)
		{
			foreach (Vector2I location in LegalMoves.CheckedRoyals)
				Colors.Set(Colors.Enum.Check, location.X, location.Y);
			Audio.Play(isCheckmate ? Audio.Enum.Checkmate : Audio.Enum.Check);
			return;
		}
		if (LegalMoves.CheckResponseZones.Count <= j)
			return;
		List<Vector2I> zone = LegalMoves.CheckResponseZones[j];
		if (CancelCheckEarly || zone.Count < i)
			return;
		if (i == 1 && !CheckAnimationsStarted.Contains(LegalMoves.RoyalAttackers[j])) CheckAnimationsStarted.Add(LegalMoves.RoyalAttackers[j]);
		else if (i == 1) return;
		if (i == 1) ActiveCheckAnimation = true;

		List<Color> previousColors = new();
		if (j == firstCheckZone)
			Colors.ChangeTileColorBack();
		if (i >= zone.Count)
			Colors.Set(Colors.Enum.Check, LegalMoves.CheckedRoyals[j].X, LegalMoves.CheckedRoyals[j].Y);
		else
		{
			PreviousCheckTiles.Add(zone[i]);
			Colors.Set(Colors.Enum.Check, zone[i].X, zone[i].Y);
		}

		if (i >= LegalMoves.maxResponseRange)
		{
			Audio.Play(isCheckmate ? Audio.Enum.Checkmate : Audio.Enum.Check);
			if (isCheckmate)
				CheckmateColors();
			ActiveCheckAnimation = false;
			return;
		}
		Timer timer = new() { WaitTime = animationSpeed/LegalMoves.maxResponseRange*durationMultiplier, OneShot = true };
		main.AddChild(timer);
		timer.Timeout += () =>
		{
			CheckAnimation(i+1, main, j, durationMultiplier);
		};
		timer.Start();
	}
	private static void CheckmateColors()
	{
		foreach (Vector2I royalLocation in LegalMoves.CheckedRoyals)
			Colors.Set(Colors.Enum.Checkmate, royalLocation.X, royalLocation.Y);
	}
	public static void CancelEarly()
	{
		if (ActiveTweens.Count == 0)
			return;
		CancelCastlingEarly = true;
		promotionUnsafe = false;
		for (int i = ActiveTweens.Count-1; i >= 0; i--)
		{
			ActiveTweens.Keys.Last().Kill();
			(Sprite2D spr, bool deleteOnFinish, float? transparency) tweenValue = ActiveTweens.Last().Value;
			Sprite2D handledSprite = tweenValue.spr;
			if (tweenValue.deleteOnFinish)
				handledSprite.QueueFree();
			else if (tweenValue.transparency != null)
				handledSprite.Modulate = new(handledSprite.Modulate.R, handledSprite.Modulate.G, handledSprite.Modulate.B, (float)tweenValue.transparency);
			ActiveTweens.Remove(ActiveTweens.Keys.Last());
		}
	}
	public static void ActiveAllCheckAnimationZones()
	{
		for (int i = 0; i < LegalMoves.CheckResponseZones.Count; i++)
			CheckAnimation(1, ((SceneTree)Engine.GetMainLoop()).CurrentScene, i, 1);
	}
	public static void CheckAnimationCancelEarly(Vector2I flatMousePosition)
	{
		if (LegalMoves.CheckedRoyals.Contains(flatMousePosition) && !CancelCheckEarly && !Audio.playedCheck)
		{
			ActiveCheckAnimation = false;
			Colors.ChangeTileColorBack();
			CancelCheckEarly = true;
			Audio.Play(Audio.Enum.Check);
		}
	}
}
