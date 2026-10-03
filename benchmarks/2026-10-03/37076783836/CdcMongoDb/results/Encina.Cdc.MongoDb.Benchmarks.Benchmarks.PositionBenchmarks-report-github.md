```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 137.530 ns |   8.366 ns | 0.4586 ns | 19.26 |    0.16 | 0.0038 |      96 B |        4.00 |
| CreatePosition   |   7.141 ns |   1.158 ns | 0.0635 ns |  1.00 |    0.01 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 679.397 ns |  11.134 ns | 0.6103 ns | 95.14 |    0.73 | 0.0401 |    1024 B |       42.67 |
| ToBytes          | 673.630 ns | 115.073 ns | 6.3076 ns | 94.33 |    1.05 | 0.0401 |    1008 B |       42.00 |
