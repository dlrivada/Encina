```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.304 ns | 0.0031 ns | 0.0044 ns |  0.11 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 11.593 ns | 0.3645 ns | 0.5456 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.534 ns | 0.0644 ns | 0.0964 ns |  0.56 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  7.141 ns | 0.1271 ns | 0.1902 ns |  0.62 |    0.03 | 0.0019 |      32 B |        1.33 |
