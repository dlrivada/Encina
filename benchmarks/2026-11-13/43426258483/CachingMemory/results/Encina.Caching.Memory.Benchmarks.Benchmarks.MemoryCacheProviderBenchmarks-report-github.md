```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.21 ns | 0.787 ns | 0.043 ns |  0.32 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.16 ns | 0.589 ns | 0.032 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 136.53 ns | 4.859 ns | 0.266 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 100.61 ns | 6.863 ns | 0.376 ns |  0.74 |      - |         - |        0.00 |
