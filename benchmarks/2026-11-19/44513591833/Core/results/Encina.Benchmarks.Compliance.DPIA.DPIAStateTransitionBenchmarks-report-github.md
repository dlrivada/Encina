```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,286.5 ns | 469.5 ns | 25.73 ns |  1.00 |    0.02 |    2 | 0.0553 | 0.0267 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  |   721.2 ns | 155.1 ns |  8.50 ns |  0.56 |    0.01 |    1 | 0.0219 | 0.0210 |     384 B |        0.41 |
| &#39;Request revision&#39;   |   720.0 ns | 266.4 ns | 14.60 ns |  0.56 |    0.01 |    1 | 0.0219 | 0.0210 |     384 B |        0.41 |
| &#39;Expire assessment&#39;  |   624.2 ns | 104.6 ns |  5.73 ns |  0.49 |    0.01 |    1 | 0.0210 | 0.0200 |     368 B |        0.39 |
