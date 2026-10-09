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
| ComparePositions | 0.7127 ns | 0.2585 ns | 0.0142 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 3.4804 ns | 0.9281 ns | 0.0509 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.8273 ns | 1.9330 ns | 0.1060 ns |  1.10 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.8304 ns | 4.1192 ns | 0.2258 ns |  1.10 |    0.06 | 0.0019 |      32 B |        1.33 |
