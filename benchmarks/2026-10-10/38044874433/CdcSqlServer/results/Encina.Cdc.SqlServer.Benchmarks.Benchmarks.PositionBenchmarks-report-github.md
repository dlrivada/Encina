```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.8834 ns | 0.7677 ns | 0.0421 ns |  0.17 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.3019 ns | 4.2895 ns | 0.2351 ns |  1.00 |    0.05 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 6.0992 ns | 5.0417 ns | 0.2764 ns |  1.15 |    0.06 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.0865 ns | 4.2787 ns | 0.2345 ns |  1.34 |    0.06 | 0.0004 |      32 B |        1.33 |
