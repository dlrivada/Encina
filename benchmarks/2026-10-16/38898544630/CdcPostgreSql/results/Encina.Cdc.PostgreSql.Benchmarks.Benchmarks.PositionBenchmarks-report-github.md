```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.40GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.8444 ns | 0.0959 ns | 0.0053 ns |  0.17 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 4.8410 ns | 0.5767 ns | 0.0316 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 5.7574 ns | 1.7463 ns | 0.0957 ns |  1.19 |    0.02 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 5.5912 ns | 1.5064 ns | 0.0826 ns |  1.16 |    0.02 | 0.0004 |      32 B |        1.33 |
