using Godot;
using System;

public partial class Animations : Update
{
	public const float animationSpeed = 0.3f;
	public static bool promotionUnsafe = false;
	public static void Tween(Sprite2D spr, float duration, Vector2I startPosition, Vector2? endPosition, float? endScale, float? endTransparency, bool deleteOnFinished, bool promotion = false, bool deleteFromPiecesDict = true, int chainIterator = int.MaxValue)
	{
		Tween tween = spr.CreateTween();
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
		OnFinishedDelete(spr, startPosition, tween, endPosition, duration, deleteOnFinished, promotion, deleteFromPiecesDict, chainIterator);
	}
	private static void TweenSetup(Sprite2D spr, Tween tween, string type, Variant end, float duration)
	{
		tween.Parallel().TweenProperty(spr, type, end, duration).SetTrans(Godot.Tween.TransitionType.Sine).SetEase(Godot.Tween.EaseType.InOut);
	}
	private static void OnFinishedPosition(Sprite2D spr)
	{
		spr.ZIndex = 1;
	}
	private static void OnFinishedDelete(Sprite2D spr, Vector2I startPosition, Tween tween, Vector2? endPosition, float duration, bool deleteOnFinished, bool promotion, bool deleteFromPiecesDict, int chainIterator)
	{
		if (deleteOnFinished && deleteFromPiecesDict)
			UpdatePosition.DeletePiece(startPosition, (Vector2I?)endPosition, false, false, '\0', spr);
		if (promotion)
			promotionUnsafe = true;
		if (duration == 0)
		{
			if (deleteOnFinished)
				spr.QueueFree();
			return;
		}
		Timer timer = new() { WaitTime = duration, OneShot = true };
		spr.AddChild(timer);
		timer.Timeout += () =>
		{
			timer.QueueFree();
			if (chainIterator < Castling.elipseQuality)
			{
				Tween(spr, animationSpeed/Castling.elipseQuality, startPosition, Castling.CalculatePointOnElipse(chainIterator+1, startPosition, Castling.endX), null, null, false, false, false, chainIterator+1);
				return;
			}
			if (deleteOnFinished)
				spr.QueueFree();
			promotionUnsafe = false;
			if (promotion && Promotion.promotionPending != null)
				Promotion.Promote((Vector2I)Promotion.promotionPending);
		};
		timer.Start();
	}
}
