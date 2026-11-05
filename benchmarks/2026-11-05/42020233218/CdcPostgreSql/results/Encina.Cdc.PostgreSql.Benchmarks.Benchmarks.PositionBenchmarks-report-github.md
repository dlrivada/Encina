```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4577 ns | 0.0806 ns | 0.0044 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.6565 ns | 0.6041 ns | 0.0331 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.9049 ns | 0.9901 ns | 0.0543 ns |  1.07 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.6210 ns | 1.2852 ns | 0.0704 ns |  0.99 |    0.02 | 0.0019 |      32 B |        1.33 |
