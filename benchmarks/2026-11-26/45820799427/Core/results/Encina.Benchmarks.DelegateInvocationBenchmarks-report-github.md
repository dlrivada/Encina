```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     28.14 ns |     1.224 ns |   0.067 ns |     1.30 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     21.70 ns |     1.891 ns |   0.104 ns |     1.00 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 23,836.54 ns | 2,647.213 ns | 145.103 ns | 1,098.39 |    7.36 | 0.0610 | 0.0305 |    5288 B |       47.21 |
| GenericTypeConstruction |     94.15 ns |     8.013 ns |   0.439 ns |     4.34 |    0.03 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     68.62 ns |    18.122 ns |   0.993 ns |     3.16 |    0.04 | 0.0020 |      - |     176 B |        1.57 |
