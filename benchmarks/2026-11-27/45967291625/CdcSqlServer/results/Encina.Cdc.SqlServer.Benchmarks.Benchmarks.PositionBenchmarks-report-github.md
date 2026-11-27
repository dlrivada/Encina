```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.5033 ns | 0.0704 ns | 0.0039 ns |  0.13 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.9938 ns | 2.9523 ns | 0.1618 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 4.5210 ns | 4.8280 ns | 0.2646 ns |  1.13 |    0.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.8081 ns | 2.8962 ns | 0.1588 ns |  0.95 |    0.05 | 0.0019 |      32 B |        1.33 |
