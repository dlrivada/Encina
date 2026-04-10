```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.305 ns | 0.0685 ns | 0.0038 ns |  0.11 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 11.544 ns | 7.6041 ns | 0.4168 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.578 ns | 2.2988 ns | 0.1260 ns |  0.57 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.088 ns | 2.5993 ns | 0.1425 ns |  0.61 |    0.02 | 0.0019 |      32 B |        1.33 |
