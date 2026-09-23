```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 154.77 ns |  2.241 ns | 0.123 ns | 10.84 |    0.51 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  14.31 ns | 14.171 ns | 0.777 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 610.65 ns | 42.732 ns | 2.342 ns | 42.76 |    2.03 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 652.01 ns | 76.331 ns | 4.184 ns | 45.65 |    2.18 | 0.0601 |    1008 B |       42.00 |
