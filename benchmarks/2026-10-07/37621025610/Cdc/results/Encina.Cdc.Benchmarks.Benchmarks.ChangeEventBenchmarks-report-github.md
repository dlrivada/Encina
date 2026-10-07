```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.7956 ns | 4.0515 ns | 0.2221 ns |  0.66 |    0.01 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9926 ns | 0.6812 ns | 0.0373 ns |  0.03 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.0755 ns | 1.2569 ns | 0.0689 ns |  0.39 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.6553 ns | 8.7445 ns | 0.4793 ns |  1.00 |    0.02 | 0.0033 |      56 B |        1.00 |
