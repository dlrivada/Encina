```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.314 ns | 0.0893 ns | 0.0049 ns |  0.12 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 10.796 ns | 3.9767 ns | 0.2180 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.893 ns | 0.2238 ns | 0.0123 ns |  0.64 |    0.01 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.849 ns | 1.8619 ns | 0.1021 ns |  0.63 |    0.01 | 0.0019 |      32 B |        1.33 |
