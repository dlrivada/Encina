```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9955 ns | 0.1944 ns | 0.0107 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.5727 ns | 1.7324 ns | 0.0950 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 8.1222 ns | 0.2587 ns | 0.0142 ns |  1.24 |    0.02 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 8.3869 ns | 0.9601 ns | 0.0526 ns |  1.28 |    0.02 | 0.0004 |      32 B |        1.33 |
