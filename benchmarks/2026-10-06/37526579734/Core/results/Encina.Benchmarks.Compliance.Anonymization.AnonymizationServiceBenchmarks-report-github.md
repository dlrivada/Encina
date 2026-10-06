```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,166.6 ns |  5,720.5 ns |   313.56 ns |  1.00 |    0.05 |    5 |  0.2289 | 0.0763 |    4226 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,364.5 ns |  1,567.4 ns |    85.91 ns |  1.03 |    0.03 |    5 |  0.2441 | 0.1068 |    4282 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,657.2 ns |  1,992.6 ns |   109.22 ns |  0.33 |    0.02 |    3 |  0.0343 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,433.0 ns |  1,330.3 ns |    72.92 ns |  0.18 |    0.01 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,494.8 ns |  3,235.8 ns |   177.36 ns |  0.55 |    0.03 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     743.4 ns |    156.4 ns |     8.57 ns |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 185,413.9 ns | 42,031.2 ns | 2,303.87 ns | 22.73 |    0.79 |    6 | 17.3340 | 0.4883 |  291296 B |       68.93 |
