```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.84GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.318 ns | 0.0659 ns | 0.0036 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 9.061 ns | 1.4360 ns | 0.0787 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.188 ns | 2.9383 ns | 0.1611 ns |  0.79 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.153 ns | 5.2728 ns | 0.2890 ns |  0.79 |    0.03 | 0.0019 |      32 B |        1.33 |
