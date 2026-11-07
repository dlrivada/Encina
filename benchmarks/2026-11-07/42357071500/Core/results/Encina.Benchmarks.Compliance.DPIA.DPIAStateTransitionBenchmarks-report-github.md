```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error    | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,802.0 ns | 344.1 ns | 18.86 ns |  1.00 |    2 | 0.0553 | 0.0267 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  |   994.1 ns | 204.9 ns | 11.23 ns |  0.55 |    1 | 0.0210 | 0.0191 |     368 B |        0.39 |
| &#39;Request revision&#39;   |   987.8 ns | 357.0 ns | 19.57 ns |  0.55 |    1 | 0.0210 | 0.0191 |     368 B |        0.39 |
| &#39;Expire assessment&#39;  |   870.7 ns | 264.6 ns | 14.50 ns |  0.48 |    1 | 0.0210 | 0.0200 |     368 B |        0.39 |
