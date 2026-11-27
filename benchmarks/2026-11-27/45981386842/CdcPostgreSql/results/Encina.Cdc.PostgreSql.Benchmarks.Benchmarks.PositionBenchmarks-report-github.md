```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.385 ns | 1.063 ns | 0.0583 ns |  0.21 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.521 ns | 1.391 ns | 0.0762 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 8.036 ns | 3.757 ns | 0.2059 ns |  1.23 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.544 ns | 1.920 ns | 0.1053 ns |  1.16 |    0.02 | 0.0019 |      32 B |        1.33 |
