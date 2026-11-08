```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    43.85 μs |  2.027 μs |  2.906 μs |  1.00 |    0.09 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   504.75 μs |  6.942 μs |  9.955 μs | 11.56 |    0.73 |  146.63 KB |       51.28 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,574.78 μs | 19.091 μs | 28.574 μs | 81.85 |    4.95 | 1341.24 KB |      469.07 |
| MarkAsProcessedAsync                |   138.37 μs |  6.607 μs |  9.262 μs |  3.17 |    0.28 |    12.2 KB |        4.27 |
