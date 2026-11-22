```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method             | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  45.84 ns | 1.143 ns | 1.711 ns |  0.32 |    0.01 |      - |         - |        0.00 |
| ExistsAsync_True   |  51.28 ns | 0.674 ns | 1.009 ns |  0.36 |    0.01 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 141.98 ns | 1.312 ns | 1.964 ns |  1.00 |    0.02 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  99.39 ns | 1.021 ns | 1.465 ns |  0.70 |    0.01 |      - |         - |        0.00 |
