```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.4909 ns | 0.6933 ns | 0.0380 ns |  0.16 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 3.1056 ns | 1.1973 ns | 0.0656 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.4246 ns | 3.6306 ns | 0.1990 ns |  1.10 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.7379 ns | 4.1484 ns | 0.2274 ns |  1.20 |    0.07 | 0.0019 |      32 B |        1.33 |
