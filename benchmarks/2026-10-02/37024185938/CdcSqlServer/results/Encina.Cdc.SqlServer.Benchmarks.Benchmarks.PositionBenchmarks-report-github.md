```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.277 ns |  0.0581 ns | 0.0032 ns |  0.12 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 10.646 ns | 11.4613 ns | 0.6282 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  6.851 ns |  0.1401 ns | 0.0077 ns |  0.64 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.502 ns |  1.5374 ns | 0.0843 ns |  0.61 |    0.03 | 0.0019 |      32 B |        1.33 |
