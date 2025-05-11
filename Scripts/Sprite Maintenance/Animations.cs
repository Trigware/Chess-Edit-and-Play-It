using Godot;
using System;
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
	public static void TweenPauseMenu(Sprite2D spr, Layer layer, bool pausing)
	{
		int yPauseMenuUnpause = isFlipped ? 0 : tileCount.Y;
		Vector2 startPauseMenuPosition = PauseMenu.GetStandardPosition(layer), endPauseMenuPosition = new Vector2(boardCenter.X, pausing ? boardCenter.Y : yPauseMenuUnpause) + PauseMenu.GetPositionalOffsetForCloseButton(layer);
		spr.Position = CalculateTilePosition(startPauseMenuPosition.X, startPauseMenuPosition.Y);
		Tween(spr, PauseMenu.PauseMenuMoveDuration, default, endPauseMenuPosition, endTransparency: pausing ? PauseMenu.PauseMenuMaxVisibilityTransparency : 0, layer: layer);
	}
	public static void Tween(Sprite2D spr, float duration, Vector2I startPosition, Vector2? endPosition, float? endScale = null, float? endTransparency = null, bool deleteOnFinished = false, bool promotion = false, bool deleteFromPiecesDict = true, int chainIterator = -1, int castlingAnimation = -1, bool promotionConfirmation = false, Tween.TransitionType transition = Godot.Tween.TransitionType.Sine, Tween.EaseType? easeType = Godot.Tween.EaseType.InOut, Layer layer = Layer.Piece, bool castlerAnimation = false)
	{
		Tween tween = spr.CreateTween();
		ActiveTweens.Add(tween, (spr, deleteOnFinished, endTransparency));
		if (layer == Layer.PauseMain)
		{
			PauseMenu.ActiveTweensCount++;
			PauseMenu.MenuMoving = true;
		}
		if (endPosition != null)
		{
			Vector2 usedEndPos = (Vector2)endPosition;
			TweenSetup(spr, tween, "position", CalculateTilePosition(usedEndPos.X, usedEndPos.Y), duration, transition, easeType);
			if (layer == Layer.Piece || layer == Layer.Promotion)
			{
				spr.ZIndex = (int)layer + 1;
				TimeControl.HandleTimerPauseProperty(Position.ColorToMove, true);
			}
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
		OnAnimationFinished(spr, startPosition, tween, endPosition, duration, deleteOnFinished, promotion, deleteFromPiecesDict, chainIterator, castlingAnimation, promotionConfirmation, layer, castlerAnimation);
	}
	private static void TweenSetup(Sprite2D spr, Tween tween, string type, Variant end, float duration, Tween.TransitionType transition, Tween.EaseType? easeType)
	{
		PropertyTweener property = tween.Parallel().TweenProperty(spr, type, end, duration).SetTrans(transition);
		if (easeType != null) property.SetEase(easeType ?? default);
	}
	private static void OnAnimationFinished(Sprite2D spr, Vector2I startPosition, Tween tween, Vector2? endPosition, float duration, bool deleteOnFinished, bool promotion, bool deleteFromPiecesDict, int chainIterator, int castlingAnimation, bool promotionConfirmation, Layer layer, bool castlerAnimation)
	{
		if (deleteOnFinished && deleteFromPiecesDict)   
			UpdatePosition.DeletePiece(startPosition, (Vector2I?)endPosition, false, false, '\0', spr);
		if (promotion)
			promotionUnsafe = true;
		if (duration == 0)
		{
			AnimationEnd(tween, deleteOnFinished, spr, endPosition, layer, castlerAnimation);
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
				castlingAnimation = castlingAnimation == Castling.endXpositions.Count ? castlingAnimation - 1 : castlingAnimation;
				Tween(spr, animationSpeed / Castling.elipseQuality, startPosition, Castling.CalculatePointOnElipse(chainIterator + 1, startPosition, Castling.endXpositions[castlingAnimation], Castling.elipsePathUp[castlingAnimation]), null, null, false, false, false, chainIterator + 1, castlingAnimation, castlerAnimation: true);
				ActiveTweens.Remove(tween);
				return;
			}
			promotionUnsafe = false;
			if (castlingAnimation >= 0)
			{
				Castling.endXpositions.RemoveAt(0);
				Castling.elipsePathUp.RemoveAt(0);
			}
			AnimationEnd(tween, deleteOnFinished, spr, endPosition, layer, castlerAnimation);
			if (Promotion.promotionPending != null)
				Promotion.Promote((Vector2I)Promotion.promotionPending);
			if (promotionConfirmation)
				Promotion.MoveHistoryDisable = false;
		};
		timer.Start();
	}
	private static void AnimationEnd(Tween tween, bool deleteOnFinished, Sprite2D spr, Vector2? endPosition, Layer layer, bool castlerAnimation)
	{
		ActiveTweens.Remove(tween);
		if (deleteOnFinished)
			spr.QueueFree();
		if (layer == Layer.Piece || layer == Layer.Promotion)
			spr.ZIndex = (int)Layer.Piece;
		if (layer == Layer.PauseMain)
		{
			PauseMenu.ActiveTweensCount--;
			if (PauseMenu.ActiveTweensCount == 0)
                PauseMenu.MenuMoving = false;
        }
		firstCheckZone = 0;
		if (Position.GameEndState != Position.EndState.Ongoing && Position.GameEndState != Position.EndState.Checkmate) return;
		if (ActiveTweens.Count == 0 && Promotion.PromotionOptionsPieces.Count == 0 && History.activeMoveSuccessionTimers == 0)
			FlipBoard();
		if (endPosition == null)
			return;
		Vector2I endNotNull = (Vector2I)endPosition;
		if (PauseMenu.UndoingMovesForNewGame && layer == Layer.Piece && !castlerAnimation)
			PauseMenu.UndoMoveForNewGame();
		for (int i = 0; i < LegalMoves.RoyalAttackers.Count; i++)
		{
			if (LegalMoves.RoyalAttackers[i] == endNotNull)
				CheckAnimation(1, ((SceneTree)Engine.GetMainLoop()).CurrentScene, i, endNotNull, false);
		}
	}
	public static void CheckAnimation(int i, Node main, int j, Vector2I attackerPosition, bool discoveredCheck, float durationMultiplier = 1)
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

		int checkmateAnimationRange = discoveredCheck ? LegalMoves.maxResponseRange : LegalMoves.CheckResponseRange[attackerPosition];
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

		if (i >= checkmateAnimationRange)
		{
			Audio.Play(isCheckmate ? Audio.Enum.Checkmate : Audio.Enum.Check);
			if (isCheckmate)
			{
				CheckmateColors();
				History.TimerCountdown(PauseMenu.PauseScreenAfterGameEndDuration, History.TimerType.GameEndScreen);
			}
			ActiveCheckAnimation = false;
			return;
		}
		Timer timer = new() { WaitTime = animationSpeed/checkmateAnimationRange*durationMultiplier, OneShot = true };
		main.AddChild(timer);
		timer.Timeout += () =>
		{
			CheckAnimation(i+1, main, j, attackerPosition, discoveredCheck, durationMultiplier);
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
			CheckAnimation(1, ((SceneTree)Engine.GetMainLoop()).CurrentScene, i, LegalMoves.CheckResponseZones[i][0], true, 1);
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
