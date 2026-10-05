```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 155.424 ns |  4.694 ns | 0.2573 ns | 21.14 |    0.19 | 0.0010 |      96 B |        4.00 |
| CreatePosition   |   7.352 ns |  1.391 ns | 0.0763 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 608.493 ns | 76.756 ns | 4.2073 ns | 82.77 |    0.89 | 0.0114 |    1024 B |       42.67 |
| ToBytes          | 581.462 ns | 18.860 ns | 1.0338 ns | 79.09 |    0.72 | 0.0114 |    1008 B |       42.00 |
