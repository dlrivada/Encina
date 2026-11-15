```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.51 ns | 0.110 ns | 0.155 ns |  0.33 |      - |         - |        0.00 |
| ExistsAsync_True   |  49.91 ns | 0.033 ns | 0.043 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 133.75 ns | 0.402 ns | 0.563 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  97.22 ns | 0.416 ns | 0.596 ns |  0.73 |      - |         - |        0.00 |
