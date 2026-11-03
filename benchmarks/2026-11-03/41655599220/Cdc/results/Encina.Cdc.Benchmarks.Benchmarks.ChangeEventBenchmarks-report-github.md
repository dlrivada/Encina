```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.5361 ns |  4.0833 ns | 0.2238 ns |  0.63 |    0.03 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9810 ns |  0.0385 ns | 0.0021 ns |  0.03 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.1076 ns |  1.8238 ns | 0.1000 ns |  0.38 |    0.02 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 29.4577 ns | 31.3325 ns | 1.7174 ns |  1.00 |    0.07 | 0.0033 |      56 B |        1.00 |
