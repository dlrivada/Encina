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
| ComparePositions | 133.96 ns |  7.806 ns | 0.428 ns |  8.96 |    0.32 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  14.96 ns | 11.596 ns | 0.636 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 676.95 ns | 95.250 ns | 5.221 ns | 45.29 |    1.66 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 591.27 ns | 92.560 ns | 5.074 ns | 39.56 |    1.46 | 0.0601 |    1008 B |       42.00 |
