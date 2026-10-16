```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 131.145 ns |   6.545 ns | 0.3587 ns | 17.09 |    0.12 | 0.0010 |      96 B |        4.00 |
| CreatePosition   |   7.676 ns |   1.091 ns | 0.0598 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 547.136 ns | 118.803 ns | 6.5120 ns | 71.28 |    0.88 | 0.0114 |    1024 B |       42.67 |
| ToBytes          | 516.182 ns |  54.672 ns | 2.9968 ns | 67.25 |    0.56 | 0.0114 |    1008 B |       42.00 |
