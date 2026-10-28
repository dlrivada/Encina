```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.892 ns | 9.7734 ns | 0.5357 ns |  0.67 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.142 ns | 0.0875 ns | 0.0048 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.753 ns | 2.9753 ns | 0.1631 ns |  0.38 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.295 ns | 3.1488 ns | 0.1726 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |
