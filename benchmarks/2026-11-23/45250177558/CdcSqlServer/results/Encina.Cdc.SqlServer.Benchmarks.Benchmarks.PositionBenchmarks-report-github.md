```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4772 ns | 0.1385 ns | 0.0076 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.7135 ns | 2.2362 ns | 0.1226 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.8599 ns | 0.4648 ns | 0.0255 ns |  1.04 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 4.1162 ns | 1.0915 ns | 0.0598 ns |  1.11 |    0.03 | 0.0019 |      32 B |        1.33 |
