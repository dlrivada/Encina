```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.88GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 2.154 μs | 2.1067 μs | 0.1155 μs |  1.00 |    0.07 |    2 | 0.0076 | 0.0038 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1.214 μs | 0.4086 μs | 0.0224 μs |  0.56 |    0.03 |    1 | 0.0038 | 0.0019 |     368 B |        0.39 |
| &#39;Request revision&#39;   | 1.151 μs | 1.4725 μs | 0.0807 μs |  0.54 |    0.04 |    1 | 0.0038 | 0.0019 |     368 B |        0.39 |
| &#39;Expire assessment&#39;  | 1.121 μs | 1.4806 μs | 0.0812 μs |  0.52 |    0.04 |    1 | 0.0038 | 0.0019 |     352 B |        0.37 |
