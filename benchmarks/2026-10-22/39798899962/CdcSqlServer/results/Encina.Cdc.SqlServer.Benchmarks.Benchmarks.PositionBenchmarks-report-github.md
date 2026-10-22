```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.5090 ns | 0.7507 ns | 0.0411 ns |  0.13 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 4.0793 ns | 4.1532 ns | 0.2277 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 4.2098 ns | 6.8052 ns | 0.3730 ns |  1.03 |    0.09 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.7715 ns | 3.3735 ns | 0.1849 ns |  0.93 |    0.06 | 0.0019 |      32 B |        1.33 |
