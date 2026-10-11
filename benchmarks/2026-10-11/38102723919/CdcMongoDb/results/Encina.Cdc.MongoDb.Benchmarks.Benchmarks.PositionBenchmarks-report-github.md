```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 133.677 ns | 0.8194 ns | 1.1487 ns | 19.23 |    1.18 | 0.0010 |      96 B |        4.00 |
| CreatePosition   |   6.977 ns | 0.3027 ns | 0.4341 ns |  1.00 |    0.09 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 521.284 ns | 3.3915 ns | 5.0763 ns | 75.00 |    4.63 | 0.0114 |    1024 B |       42.67 |
| ToBytes          | 495.029 ns | 2.3467 ns | 3.4397 ns | 71.22 |    4.37 | 0.0114 |    1008 B |       42.00 |
