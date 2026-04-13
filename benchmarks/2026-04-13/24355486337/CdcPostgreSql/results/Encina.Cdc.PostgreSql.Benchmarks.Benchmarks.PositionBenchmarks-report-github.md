```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.148 ns | 0.1133 ns | 0.1551 ns | 1.154 ns |  0.12 |    0.03 |      - |         - |        0.00 |
| CreatePosition   | 10.202 ns | 1.3029 ns | 1.9501 ns | 9.110 ns |  1.04 |    0.28 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.787 ns | 0.2032 ns | 0.2978 ns | 6.833 ns |  0.69 |    0.13 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.164 ns | 0.1387 ns | 0.2033 ns | 7.188 ns |  0.73 |    0.14 | 0.0019 |      32 B |        1.33 |
