```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.277 ns |  0.0393 ns | 0.0022 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 8.892 ns | 12.7888 ns | 0.7010 ns |  1.00 |    0.10 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.648 ns |  0.4592 ns | 0.0252 ns |  0.75 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.532 ns | 10.9700 ns | 0.6013 ns |  0.74 |    0.08 | 0.0019 |      32 B |        1.33 |
