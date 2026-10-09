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
| ComparePositions | 0.4592 ns | 0.4457 ns | 0.0244 ns |  0.14 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.2448 ns | 3.4095 ns | 0.1869 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.5886 ns | 3.9757 ns | 0.2179 ns |  1.11 |    0.08 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.6193 ns | 3.5733 ns | 0.1959 ns |  1.12 |    0.08 | 0.0019 |      32 B |        1.33 |
