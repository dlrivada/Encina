```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.37GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,312.4 ns |  88.84 ns |  4.87 ns |  1.00 |    0.00 |    2 | 0.0553 | 0.0267 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  |   750.2 ns | 477.58 ns | 26.18 ns |  0.57 |    0.02 |    1 | 0.0219 | 0.0210 |     384 B |        0.41 |
| &#39;Request revision&#39;   |   710.7 ns | 325.41 ns | 17.84 ns |  0.54 |    0.01 |    1 | 0.0219 | 0.0210 |     384 B |        0.41 |
| &#39;Expire assessment&#39;  |   640.5 ns | 539.93 ns | 29.60 ns |  0.49 |    0.02 |    1 | 0.0210 | 0.0200 |     368 B |        0.39 |
