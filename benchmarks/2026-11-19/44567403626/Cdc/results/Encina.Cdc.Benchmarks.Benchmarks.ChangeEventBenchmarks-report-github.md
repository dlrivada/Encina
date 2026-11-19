```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.56GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 19.097 ns |  2.5953 ns | 0.1423 ns |  0.65 |    0.01 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.019 ns |  0.4467 ns | 0.0245 ns |  0.03 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.265 ns |  6.5057 ns | 0.3566 ns |  0.38 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 29.494 ns | 10.6010 ns | 0.5811 ns |  1.00 |    0.02 | 0.0033 |      56 B |        1.00 |
