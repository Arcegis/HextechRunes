using HextechRunes;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes.Tests;

internal static partial class Program
{
	// 自动打出在读费用和持有者之前就放行,所以无持有者的实例也能验证;3 费形态牌开局自动打出依赖这一点。
	[HextechTest]
	private static void BackToBasicsAllowsAutoPlayedCards()
	{
		Expect(new BackToBasicsRune().ShouldPlay(new DemonForm(), AutoPlayType.Default), "Back to Basics should not block auto-played 3-cost cards");
	}
}
