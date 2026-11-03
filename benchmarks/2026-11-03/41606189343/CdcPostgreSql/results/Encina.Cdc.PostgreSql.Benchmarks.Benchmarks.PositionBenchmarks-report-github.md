```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4722 ns | 0.0980 ns | 0.0054 ns |  0.12 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.8214 ns | 1.2598 ns | 0.0691 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 4.1468 ns | 1.3818 ns | 0.0757 ns |  1.09 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.8344 ns | 0.6917 ns | 0.0379 ns |  1.00 |    0.02 | 0.0019 |      32 B |        1.33 |
