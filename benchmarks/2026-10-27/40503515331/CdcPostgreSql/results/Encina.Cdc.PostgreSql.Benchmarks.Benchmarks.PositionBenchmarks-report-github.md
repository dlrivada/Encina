```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4405 ns | 0.2705 ns | 0.0148 ns |  0.12 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.5741 ns | 2.6389 ns | 0.1446 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.7434 ns | 0.3788 ns | 0.0208 ns |  1.05 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.9216 ns | 4.3852 ns | 0.2404 ns |  1.10 |    0.07 | 0.0019 |      32 B |        1.33 |
