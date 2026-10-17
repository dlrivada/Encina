```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9778 ns | 0.1058 ns | 0.0058 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.4091 ns | 1.6146 ns | 0.0885 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 7.9489 ns | 0.8159 ns | 0.0447 ns |  1.24 |    0.02 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 6.7821 ns | 0.7120 ns | 0.0390 ns |  1.06 |    0.01 | 0.0004 |      32 B |        1.33 |
