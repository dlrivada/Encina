```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9798 ns | 0.0060 ns | 0.0087 ns |  0.15 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.5859 ns | 0.1602 ns | 0.2348 ns |  1.00 |    0.05 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.7518 ns | 0.3211 ns | 0.4805 ns |  1.18 |    0.08 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 8.7356 ns | 0.3332 ns | 0.4988 ns |  1.33 |    0.09 | 0.0004 |      32 B |        1.33 |
