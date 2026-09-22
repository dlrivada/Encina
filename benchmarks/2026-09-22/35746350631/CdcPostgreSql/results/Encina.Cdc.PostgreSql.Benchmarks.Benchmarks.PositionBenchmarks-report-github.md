```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.9772 ns | 0.0523 ns | 0.0029 ns |  0.14 |      - |         - |        0.00 |
| CreatePosition   | 6.8102 ns | 0.4511 ns | 0.0247 ns |  1.00 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 6.9005 ns | 0.4344 ns | 0.0238 ns |  1.01 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 7.8908 ns | 1.3085 ns | 0.0717 ns |  1.16 | 0.0004 |      32 B |        1.33 |
