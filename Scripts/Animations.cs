using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Animations : Update
{
	public const float animationSpeed = 5;
	public static bool promotionUnsafe = false, CancelCheckAnimationEarly = false, CancelCastlingEarly = false;
	public static List<Vector2I> PreviousCheckTiles = new();
	public static Dictionary<Tween, (Sprite2D spr, bool deleteOnFinish, float? transparency)> ActiveTweens = new();
	public static void Tween(Sprite2D spr, float duration, Vector2I startPosition, Vector2? endPosition, float? endScale, float? endTransparency, bool deleteOnFinished, bool promotion = false, bool deleteFromPiecesDict = true, int chainIterator = -1, int castlingAnimation = -1)
	{
		Tween tween = spr.CreateTween();
		ActiveTweens.Add(tween, (spr, deleteOnFinished, endTransparency));
		if (endPosition != null)
			spr.ZIndex = 2;
		if (endPosition != null)
		{
			Vector2 usedEndPos = (Vector2)endPosition;
			TweenSetup(spr, tween, "position", CalculateTilePosition(usedEndPos.X, usedEndPos.Y), duration);
		}
		if (endScale != null)
		{
			float usedEndScale = (float)endScale;
			TweenSetup(spr, tween, "scale", new Vector2(usedEndScale, usedEndScale), duration);
		}
		if (endTransparency != null)
		{
			float usedEndTransparency = (float)endTransparency;
			TweenSetup(spr, tween, "modulate:a", usedEndTransparency, duration);
		}
		if (endPosition != null)
			tween.Finished += () => OnFinishedPosition(spr);
		OnFinishedDelete(spr, startPosition, tween, endPosition, duration, deleteOnFinished, promotion, deleteFromPiecesDict, chainIterator, castlingAnimation);
	}
	private static void TweenSetup(Sprite2D spr, Tween tween, string type, Variant end, float duration)
	{
		tween.Parallel().TweenProperty(spr, type, end, duration).SetTrans(Godot.Tween.TransitionType.Sine).SetEase(Godot.Tween.EaseType.InOut);
	}
	private static void OnFinishedPosition(Sprite2D spr)
	{
		spr.ZIndex = 1;
	}
	private static void OnFinishedDelete(Sprite2D spr, Vector2I startPosition, Tween tween, Vector2? endPosition, float duration, bool deleteOnFinished, bool promotion, bool deleteFromPiecesDict, int chainIterator, int castlingAnimation)
	{
		if (deleteOnFinished && deleteFromPiecesDict)
			UpdatePosition.DeletePiece(startPosition, (Vector2I?)endPosition, false, false, '\0', spr);
		if (promotion)
			promotionUnsafe = true;
		if (duration == 0)
		{
			ActiveTweens.Remove(tween);
			if (deleteOnFinished)
				spr.QueueFree();
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
				Tween(spr, animationSpeed/Castling.elipseQuality, startPosition, Castling.CalculatePointOnElipse(chainIterator+1, startPosition, Castling.endXpositions[castlingAnimation], Castling.elipsePathUp[castlingAnimation]), null, null, false, false, false, chainIterator+1, castlingAnimation);
				return;
			}
			if (deleteOnFinished)
				spr.QueueFree();
			promotionUnsafe = false;
			ActiveTweens.Remove(tween);
			if (castlingAnimation >= 0)
			{
                Castling.endXpositions.RemoveAt(0);
				Castling.elipsePathUp.RemoveAt(0);
            }
            if (promotion && Promotion.promotionPending != null)
				Promotion.Promote((Vector2I)Promotion.promotionPending);
		};
		timer.Start();
	}
	public static void CheckAnimation(int i, Node main)
	{
		if (animationSpeed == 0)
		{
			foreach (Vector2I location in LegalMoves.CheckedRoyals)
				Colors.Set(Colors.Enum.Check, location.X, location.Y);
			Audio.Play(Position.GameEndState == Position.EndState.Checkmate ? Audio.Enum.Checkmate : Audio.Enum.Check);
			return;
		}
		if (CancelCheckAnimationEarly)
		{
			CancelCheckAnimationEarly = false;
			return;
		}
		List<Color> previousColors = new();
		Colors.ChangeTileColorBack();
		for (int j = 0; j < LegalMoves.CheckResponseZones.Count; j++)
		{
			List<Vector2I> zone = LegalMoves.CheckResponseZones[j];
			if (i >= zone.Count)
			{
				Colors.Set(Colors.Enum.Check, LegalMoves.CheckedRoyals[j].X, LegalMoves.CheckedRoyals[j].Y);
				continue;
			}
			PreviousCheckTiles.Add(zone[i]);
			Colors.Set(Colors.Enum.Check, zone[i].X, zone[i].Y);
		}
		if (i >= LegalMoves.maxResponseRange)
		{
			Audio.Play(Position.GameEndState == Position.EndState.Checkmate ? Audio.Enum.Checkmate : Audio.Enum.Check);
			if (Position.GameEndState == Position.EndState.Checkmate)
				CheckmateColors();
			return;
		}
		Timer timer = new() { WaitTime = animationSpeed/LegalMoves.maxResponseRange, OneShot = true };
		main.AddChild(timer);
		timer.Timeout += () =>
		{
			CheckAnimation(i+1, main);
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
}
