```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean     | Error     | StdDev    | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 2.456 μs | 0.1501 μs | 0.0082 μs |  1.00 |    2 | 0.0343 | 0.0305 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1.474 μs | 0.6034 μs | 0.0331 μs |  0.60 |    1 | 0.0134 | 0.0114 |     368 B |        0.39 |
| &#39;Request revision&#39;   | 1.442 μs | 0.5164 μs | 0.0283 μs |  0.59 |    1 | 0.0134 | 0.0114 |     368 B |        0.39 |
| &#39;Expire assessment&#39;  | 1.247 μs | 0.2589 μs | 0.0142 μs |  0.51 |    1 | 0.0134 | 0.0114 |     352 B |        0.37 |
