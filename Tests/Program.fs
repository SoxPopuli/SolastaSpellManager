namespace Tests

open NUnit.Framework

type Tests() =
    [<SetUp>]
    member this.Setup() = ()

    [<Test>]
    member this.Test1() = Assert.That(1, Is.EqualTo(1))
