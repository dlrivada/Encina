```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method             | Mean      | Error    | StdDev   | Median    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  53.87 ns | 0.046 ns | 0.064 ns |  53.86 ns |  0.30 |      - |         - |        0.00 |
| ExistsAsync_True   |  61.20 ns | 0.556 ns | 0.815 ns |  60.52 ns |  0.34 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 181.55 ns | 0.387 ns | 0.567 ns | 181.45 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 130.15 ns | 1.409 ns | 2.021 ns | 128.48 ns |  0.72 |      - |         - |        0.00 |
