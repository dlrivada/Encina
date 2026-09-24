```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.9028 ns | 0.0648 ns | 0.0036 ns |  0.16 |      - |         - |        0.00 |
| CreatePosition   | 5.7382 ns | 0.2083 ns | 0.0114 ns |  1.00 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 7.3815 ns | 0.5637 ns | 0.0309 ns |  1.29 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 8.2829 ns | 0.8092 ns | 0.0444 ns |  1.44 | 0.0013 |      32 B |        1.33 |
