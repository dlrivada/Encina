```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4735 ns | 0.5340 ns | 0.0293 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.7523 ns | 0.3903 ns | 0.0214 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 4.3251 ns | 3.8622 ns | 0.2117 ns |  1.15 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 4.2544 ns | 5.9531 ns | 0.3263 ns |  1.13 |    0.08 | 0.0019 |      32 B |        1.33 |
