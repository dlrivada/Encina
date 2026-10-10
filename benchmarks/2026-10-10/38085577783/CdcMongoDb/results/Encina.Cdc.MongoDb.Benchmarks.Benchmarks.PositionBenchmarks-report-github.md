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
| ComparePositions | 114.416 ns |  11.999 ns | 0.6577 ns | 17.15 |    0.36 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.673 ns |   2.824 ns | 0.1548 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 474.418 ns | 103.475 ns | 5.6718 ns | 71.12 |    1.62 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 448.001 ns |  25.713 ns | 1.4094 ns | 67.16 |    1.38 | 0.0601 |    1008 B |       42.00 |
