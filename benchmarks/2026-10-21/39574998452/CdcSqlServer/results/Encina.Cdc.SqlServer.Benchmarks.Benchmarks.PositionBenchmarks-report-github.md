```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.9753 ns | 0.3788 ns | 0.0208 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.8463 ns | 1.5780 ns | 0.0865 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 8.1845 ns | 1.6692 ns | 0.0915 ns |  1.20 |    0.02 | 0.0003 |      24 B |        1.00 |
| ToBytes          | 8.9692 ns | 0.6472 ns | 0.0355 ns |  1.31 |    0.02 | 0.0004 |      32 B |        1.33 |
