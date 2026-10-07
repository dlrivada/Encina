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
| ComparePositions | 0.9201 ns | 0.0979 ns | 0.0054 ns |  0.15 |      - |         - |        0.00 |
| CreatePosition   | 6.2269 ns | 0.7245 ns | 0.0397 ns |  1.00 | 0.0010 |      24 B |        1.00 |
| FromBytes        | 6.3284 ns | 0.2781 ns | 0.0152 ns |  1.02 | 0.0010 |      24 B |        1.00 |
| ToBytes          | 8.1026 ns | 1.3828 ns | 0.0758 ns |  1.30 | 0.0013 |      32 B |        1.33 |
