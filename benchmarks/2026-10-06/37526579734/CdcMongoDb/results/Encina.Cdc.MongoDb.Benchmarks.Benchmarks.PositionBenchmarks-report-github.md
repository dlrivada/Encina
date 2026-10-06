```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 149.013 ns |   1.9321 ns |  0.1059 ns | 21.08 |    0.09 | 0.0010 |      96 B |        4.00 |
| CreatePosition   |   7.069 ns |   0.6108 ns |  0.0335 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 605.236 ns | 221.6392 ns | 12.1488 ns | 85.62 |    1.53 | 0.0114 |    1024 B |       42.67 |
| ToBytes          | 574.155 ns |  10.7891 ns |  0.5914 ns | 81.23 |    0.34 | 0.0114 |    1008 B |       42.00 |
