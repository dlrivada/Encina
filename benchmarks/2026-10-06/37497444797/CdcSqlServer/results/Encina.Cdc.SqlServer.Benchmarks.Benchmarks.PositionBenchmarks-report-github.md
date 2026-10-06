```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.5083 ns | 0.2292 ns | 0.0126 ns |  0.15 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.2838 ns | 0.2777 ns | 0.0152 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.8507 ns | 5.2516 ns | 0.2879 ns |  1.17 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.5493 ns | 1.4346 ns | 0.0786 ns |  1.08 |    0.02 | 0.0019 |      32 B |        1.33 |
