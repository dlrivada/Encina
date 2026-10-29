```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.280 ns | 0.0503 ns | 0.0028 ns |  0.18 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 7.149 ns | 0.9792 ns | 0.0537 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 8.918 ns | 4.1751 ns | 0.2289 ns |  1.25 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.393 ns | 1.8217 ns | 0.0999 ns |  0.89 |    0.01 | 0.0019 |      32 B |        1.33 |
