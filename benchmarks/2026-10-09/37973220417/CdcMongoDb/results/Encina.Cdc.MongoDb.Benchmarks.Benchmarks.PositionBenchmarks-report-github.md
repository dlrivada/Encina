```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.692 ns |  14.397 ns |  0.7892 ns | 17.04 |    0.31 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.025 ns |   2.885 ns |  0.1581 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 558.722 ns |  62.878 ns |  3.4466 ns | 69.64 |    1.26 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 540.547 ns | 274.892 ns | 15.0678 ns | 67.37 |    2.00 | 0.0601 |    1008 B |       42.00 |
