```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error    | StdDev    | Gen0   | Allocated |
|------------------------------------------ |---------:|---------:|----------:|-------:|----------:|
| Publish_Notification_WithMultipleHandlers | 3.543 μs | 2.046 μs | 0.1121 μs | 0.2556 |    4.2 KB |
| Send_Command_WithInstrumentation          | 4.472 μs | 1.170 μs | 0.0641 μs | 0.2823 |    4.7 KB |
