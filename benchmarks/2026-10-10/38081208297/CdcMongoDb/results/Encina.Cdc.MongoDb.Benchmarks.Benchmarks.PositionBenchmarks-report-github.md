```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 112.129 ns |  55.750 ns | 3.0558 ns | 14.66 |    0.76 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   7.664 ns |   7.449 ns | 0.4083 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 467.561 ns |  18.276 ns | 1.0018 ns | 61.12 |    2.81 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 460.545 ns | 159.682 ns | 8.7527 ns | 60.20 |    2.93 | 0.0601 |    1008 B |       42.00 |
